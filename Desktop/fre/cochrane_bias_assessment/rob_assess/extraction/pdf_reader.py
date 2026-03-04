"""PDF text extraction with PyMuPDF primary, pdfplumber fallback."""

from pathlib import Path

import pymupdf


def extract_text_pymupdf(pdf_path: str | Path) -> str:
    """Extract text from PDF using PyMuPDF (fast, handles most PDFs well)."""
    doc = pymupdf.open(str(pdf_path))
    pages = []
    for page in doc:
        pages.append(page.get_text("text"))
    doc.close()
    return "\n\n".join(pages)


def extract_text_pdfplumber(pdf_path: str | Path) -> str:
    """Fallback extraction using pdfplumber (better for some table-heavy PDFs)."""
    import pdfplumber

    pages = []
    with pdfplumber.open(str(pdf_path)) as pdf:
        for page in pdf.pages:
            text = page.extract_text()
            if text:
                pages.append(text)
    return "\n\n".join(pages)


def extract_text(pdf_path: str | Path) -> str:
    """Extract text from PDF, trying PyMuPDF first, pdfplumber as fallback.

    Returns the extracted full text, or raises ValueError if both fail.
    """
    pdf_path = Path(pdf_path)
    if not pdf_path.exists():
        raise FileNotFoundError(f"PDF not found: {pdf_path}")
    if not pdf_path.suffix.lower() == ".pdf":
        raise ValueError(f"Not a PDF file: {pdf_path}")

    # Try PyMuPDF first
    text = extract_text_pymupdf(pdf_path)
    if text.strip():
        return text

    # Fallback to pdfplumber
    text = extract_text_pdfplumber(pdf_path)
    if text.strip():
        return text

    raise ValueError(f"Could not extract text from PDF: {pdf_path}")
