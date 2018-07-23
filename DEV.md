## Developer Notes

This is a cute vim hack that will copy just the tests in the current buffer and run them. 

It assumes all tests are independent and require no files but themself.

`nnoremap <leader>r :w<cr>:let $TEST_FILES=expand('%')<cr>:!make test-fast<cr>`

This runs all tests with the slighly brittle "don't run msbuild" hack, which is 2x faster:

`nnoremap <leader>a :w<cr>:!make test-fast<cr>`

This compiles things:

`nnoremap <leader>b :w<cr>:!make<cr>`
