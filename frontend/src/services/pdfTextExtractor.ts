import * as pdfjsLib from 'pdfjs-dist';

pdfjsLib.GlobalWorkerOptions.workerSrc = '/pdf.worker.min.js';

export async function extractTextFromPdf(file: File): Promise<string[]> {
  const arrayBuffer = await file.arrayBuffer();
  const pdf = await pdfjsLib.getDocument({ data: arrayBuffer }).promise;
  const pages: string[] = [];

  for (let pageNum = 1; pageNum <= pdf.numPages; pageNum++) {
    const page = await pdf.getPage(pageNum);
    const textContent = await page.getTextContent();

    let lastY: number | null = null;
    let lineText = '';
    const lines: string[] = [];

    for (const item of textContent.items) {
      if (!('str' in item)) continue;
      const typedItem = item as { str: string; transform: number[] };
      const y = typedItem.transform[5];

      if (lastY !== null && Math.abs(y - lastY) > 2) {
        lines.push(lineText.trim());
        lineText = '';
      }

      lineText += typedItem.str + ' ';
      lastY = y;
    }

    if (lineText.trim()) {
      lines.push(lineText.trim());
    }

    pages.push(lines.join('\n'));
  }

  return pages;
}
