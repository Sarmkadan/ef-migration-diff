# StringAndCollectionExtensionsTests

Unit tests for the `StringAndCollectionExtensions` class, verifying string manipulation and collection batching utility methods. These tests ensure correct behavior for case conversion, string truncation, and batching operations.

## API

### `ToPascalCase_WithUnderscoreSeparatedWords_ReturnsPascalCase()`

Verifies that underscore-separated words are correctly converted to PascalCase.

- **Parameters**: None
- **Return value**: `void`
- **Throws**: No exceptions expected under normal conditions

### `ToSnakeCase_WithPascalCaseInput_InsertsUnderscoreBeforeUppercase()`

Ensures PascalCase input is converted to snake_case with underscores inserted before uppercase letters.

- **Parameters**: None
- **Return value**: `void`
- **Throws**: No exceptions expected under normal conditions

### `Truncate_WhenStringExceedsMaxLength_ReturnsTruncatedStringWithEllipsis()`

Confirms that strings exceeding a specified maximum length are truncated and appended with an ellipsis.

- **Parameters**: None
- **Return value**: `void`
- **Throws**: No exceptions expected under normal conditions

### `Batch_WithTenItemsAndBatchSizeThree_CreatesFourBatchesWithLastPartial()`

Validates that a collection is correctly partitioned into batches of the specified size, including handling the final partial batch.

- **Parameters**: None
- **Return value**: `void`
- **Throws**: No exceptions expected under normal conditions

## Usage
