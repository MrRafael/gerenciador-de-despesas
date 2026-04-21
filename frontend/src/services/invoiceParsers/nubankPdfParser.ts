import type { InvoiceParser, CardGroup, ParsedExpense } from './types';

const MONTH_MAP: Record<string, string> = {
  'JAN': '01', 'FEV': '02', 'MAR': '03', 'ABR': '04',
  'MAI': '05', 'JUN': '06', 'JUL': '07', 'AGO': '08',
  'SET': '09', 'OUT': '10', 'NOV': '11', 'DEZ': '12',
};

const TRANSACTION_REGEX = /^(\d{2})\s(JAN|FEV|MAR|ABR|MAI|JUN|JUL|AGO|SET|OUT|NOV|DEZ)\s+(?:••••\s(\d{4})\s+)?(.+?)\s+(?:−|-)?\s*R\$\s([\d.,]+)$/;
const PERIOD_REGEX = /TRANSAÇÕES\s+DE\s+\d{2}\s\w{3}\sA\s\d{2}\s\w{3}/;
const HOLDER_HEADER_REGEX = /^(?:Compras de\s+)?(.+?)\s+(?:−|-)?\s*R\$\s[\d.,]+$/;
const PAYMENT_SECTION_REGEX = /^Pagamentos e Financiamentos/;
const NEGATIVE_VALUE_REGEX = /(?:−|-)\s*R\$/;
const IGNORED_LINES = ['Saldo restante da fatura anterior', 'Conversão:'];

function inferYear(pages: string[]): number {
  for (const page of pages) {
    const match = page.match(/FATURA\s+(\d{2})\s+\w{3}\s+(\d{4})/);
    if (match) {
      return parseInt(match[2], 10);
    }
  }
  return new Date().getFullYear();
}

function parseValue(valueStr: string): number {
  const cleaned = valueStr.replace(/\./g, '').replace(',', '.');
  return parseFloat(cleaned);
}

function formatDate(day: string, monthAbbr: string, year: number): string {
  const month = MONTH_MAP[monthAbbr];
  return `${year}-${month}-${day}`;
}

function isNegative(line: string): boolean {
  return NEGATIVE_VALUE_REGEX.test(line);
}

function shouldIgnoreLine(line: string): boolean {
  return IGNORED_LINES.some(ignored => line.includes(ignored));
}

export const nubankPdfParser: InvoiceParser = {
  bankName: 'Nubank',

  detect(pages: string[]): boolean {
    const fullText = pages.join(' ').toLowerCase();
    return fullText.includes('nu pagamentos') || fullText.includes('nubank');
  },

  parse(pages: string[]): CardGroup[] {
    const year = inferYear(pages);
    const cardMap = new Map<string, { holderName: string; expenses: ParsedExpense[] }>();
    let currentHolder = '';
    let inTransactionSection = false;
    let inPaymentSection = false;

    for (const pageText of pages) {
      const lines = pageText.split('\n');

      for (const rawLine of lines) {
        const line = rawLine.trim();
        if (!line) continue;

        if (PERIOD_REGEX.test(line)) {
          inTransactionSection = true;
          continue;
        }

        if (!inTransactionSection) continue;

        if (PAYMENT_SECTION_REGEX.test(line)) {
          inPaymentSection = true;
          continue;
        }

        if (inPaymentSection) continue;

        if (shouldIgnoreLine(line)) continue;
        if (isNegative(line)) continue;

        const holderMatch = line.match(HOLDER_HEADER_REGEX);
        if (holderMatch && !/^\d{2}\s/.test(line)) {
          const name = holderMatch[1].replace(/^Compras de\s+/i, '');
          currentHolder = name;
          continue;
        }

        const txMatch = line.match(TRANSACTION_REGEX);
        if (!txMatch) continue;

        const [, day, monthAbbr, cardDigits, description, valueStr] = txMatch;
        const value = parseValue(valueStr);

        if (value <= 0) continue;

        const cardLabel = cardDigits ? `•••• ${cardDigits}` : 'NuTag / Outros';
        const date = formatDate(day, monthAbbr, year);

        if (!cardMap.has(cardLabel)) {
          cardMap.set(cardLabel, { holderName: currentHolder, expenses: [] });
        }

        const group = cardMap.get(cardLabel)!;
        if (!group.holderName && currentHolder) {
          group.holderName = currentHolder;
        }

        group.expenses.push({ date, description: description.trim(), value });
      }
    }

    const result: CardGroup[] = [];
    for (const [cardLabel, { holderName, expenses }] of cardMap) {
      if (expenses.length === 0) continue;
      const totalValue = expenses.reduce((sum, e) => sum + e.value, 0);
      result.push({
        cardLabel,
        holderName: holderName || 'Titular',
        expenses,
        totalValue: Math.round(totalValue * 100) / 100,
      });
    }

    return result;
  },
};
