import { nubankPdfParser } from './nubankPdfParser';
import { santanderPdfParser } from './santanderPdfParser';
import type { InvoiceParser } from './types';

const parsers: InvoiceParser[] = [nubankPdfParser, santanderPdfParser];

export function detectParser(pages: string[]): InvoiceParser | null {
  return parsers.find(p => p.detect(pages)) ?? null;
}

export { type InvoiceParser, type CardGroup, type ParsedExpense } from './types';
