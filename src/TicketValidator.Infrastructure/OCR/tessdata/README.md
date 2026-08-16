# Spanish Tesseract Data

`spa.traineddata` is obtained from the official
[`tesseract-ocr/tessdata_fast`](https://github.com/tesseract-ocr/tessdata_fast)
repository.

The Infrastructure project copies every `*.traineddata` file in this directory
to `tessdata/` in build and publish output. The default OCR configuration then
resolves it from `AppContext.BaseDirectory/tessdata`.
