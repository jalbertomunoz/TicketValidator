# Tesseract Data

`spa.traineddata` is obtained from the official
[`tesseract-ocr/tessdata_fast`](https://github.com/tesseract-ocr/tessdata_fast)
repository.

`osd.traineddata` is obtained from the official
[`tesseract-ocr/tessdata_fast`](https://github.com/tesseract-ocr/tessdata_fast)
repository. It provides Tesseract's orientation and script detection model for
the 0, 90, 180 and 270 degree correction performed before OCR.

The Infrastructure project copies every `*.traineddata` file in this directory
to `tessdata/` in build and publish output. The default OCR configuration then
resolves it from `AppContext.BaseDirectory/tessdata`.

These files are public Tesseract language models and contain no ticket or
personal data.
