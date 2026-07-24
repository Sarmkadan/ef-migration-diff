# CommandParser Input Validation Improvements

## Summary
Added comprehensive input validation to the `CommandParser` class public entry points to ensure malformed input fails fast with clear error messages rather than propagating into I/O or formatter code.

## Changes Made

### 1. Added Constant for Maximum Argument Length
- Added `MaxArgumentLength = 32 * 1024` (32KB) constant to prevent denial-of-service via oversized arguments
- Applied consistently to both raw arguments and parsed values

### 2. Enhanced `Parse()` Method (Lines 59-139)

#### New Validations:
- **Null checks**: All parameters now have explicit null validation using `ArgumentNullException.ThrowIfNull()`
- **Whitespace validation**: Added `ArgumentException.ThrowIfNullOrWhiteSpace()` for commandName
- **Argument length validation**: Each argument is checked for:
  - Maximum length (32KB)
  - Null, empty, or whitespace content
  - Clear error messages with position information

#### XML Documentation Updates:
- Enhanced parameter descriptions to indicate "Cannot be null/whitespace" where applicable
- Added `<exception>` tags for all exceptions that can be thrown
- Documented ArgumentNullException and ArgumentException with clear conditions

### 3. Enhanced `Validate()` Method (Lines 216-396)

#### New Validations:
- **Option value validation**: Checks parsed option values for:
  - Empty or whitespace content (for --format, --dot)
  - Maximum length limits
  - Invalid path characters (for --dot option)
  
- **Positional argument validation**: Validates all parsed positional arguments for:
  - Empty or whitespace content
  - Maximum length limits
  - Clear error messages with position information

#### XML Documentation Updates:
- Enhanced parameter descriptions to indicate "Cannot be null/empty" where applicable
- Added comprehensive `<exception>` tags for all exceptions
- Documented ArgumentNullException and ArgumentException with clear conditions

### 4. Enhanced `GenerateUsage()` Method (Lines 379-411)

#### XML Documentation Updates:
- Enhanced parameter descriptions
- Added `<exception>` tag for ArgumentException

## Validation Coverage

### Input Types Validated:
1. **commandName parameter**: Null, empty, whitespace
2. **args array**: Null, empty array, individual argument null/empty/whitespace, oversized arguments
3. **serviceProvider parameter**: Null
4. **Parsed option values** (--format, --dot): Empty, whitespace, oversized, invalid characters
5. **Parsed positional arguments**: Empty, whitespace, oversized

### Error Handling:
- All validations throw appropriate exceptions (ArgumentNullException, ArgumentException)
- Error messages are clear and actionable
- Includes context (position, option name, length) for debugging
- Fails fast - validation happens before any I/O operations

## Quality Bar Compliance

✅ **Guard clauses first**: All public methods have ArgumentNullException.ThrowIfNull() at the top
✅ **Modern C#**: Uses ArgumentException.ThrowIfNullOrWhiteSpace() and string.IsNullOrWhiteSpace()
✅ **XML doc comments**: Every new public member has XML documentation with `<exception>` tags
✅ **Build passes**: Solution builds successfully with no new errors
✅ **No test modifications**: As per requirements, no test files were modified
✅ **No project modifications**: No .csproj/.sln files were touched
✅ **No package additions**: Only used existing BCL functionality

## Impact

### Before:
- Malformed input could propagate to I/O operations (File.WriteAllText, etc.)
- No length limits on arguments
- No validation of empty/whitespace values
- Generic exceptions without context

### After:
- Input validation happens immediately in Parse() and Validate()
- Clear, actionable error messages
- Length limits prevent denial-of-service
- Empty/whitespace values caught early
- All public entry points have comprehensive XML documentation with exceptions

## Files Modified
- `/home/redrocket/task-factory/workdir/ef-migration-diff/CLI/CommandParser.cs`

## Testing
The existing test suite continues to pass. The validation improvements are backward compatible - valid inputs work exactly as before, while invalid inputs now fail fast with clear error messages instead of causing downstream issues.
