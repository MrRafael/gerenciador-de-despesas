import type { InvoiceParser, CardGroup, ParsedExpense } from './types';

const CARD_HEADER_REGEX = /^(?:@\s*)?(.+?)\s*-\s*(\d{4}\s*X{4}\s*X{4}\s*\d{4})/;
const TRANSACTION_REGEX = /^(?:3\s+)?(\d{2}\/\d{2})\s+(.+?)\s{2,}(?:(\d{2}\/\d{2})\s{2,})?(-?[\d.,]+)$/;

const IGNORED_SECTIONS = [
  'VALOR TOTAL',
  'Resumo da Fatura',
  'Saldo Anterior',
  'Saldo total consolidado',
  'Saldo Desta Fatura',
  'Total Despesas',
  'Total de pagamentos',
  'Total de créditos',
  'Compras parceladas',
];

const IGNORED_LINE_PATTERNS = [
  /^Compra\s+Data\s+Descri/,
  /^Descrição\s+R\$/,
  /^crédito e tarifas/,
];

function inferYear(pages: string[]): number {
  for (const page of pages) {
    const match = page.match(/(\d{2})\/(\d{2})\/(\d{4})/);
    if (match) {
      return parseInt(match[3], 10);
    }
  }
  return new Date().getFullYear();
}

function inferInvoiceMonth(pages: string[]): number {
  for (const page of pages) {
    const match = page.match(/Vencimento[\s\S]*?(\d{2})\/(\d{2})\/(\d{4})/);
    if (match) {
      return parseInt(match[1], 10);
    }
  }
  return new Date().getMonth() + 1;
}

function parseValue(valueStr: string): number {
  const cleaned = valueStr.replace(/\./g, '').replace(',', '.');
  return parseFloat(cleaned);
}

function formatDate(dayMonth: string, year: number, invoiceMonth: number): string {
  const [day, month] = dayMonth.split('/').map(s => parseInt(s, 10));
  let adjustedYear = year;
  if (month > invoiceMonth + 1) {
    adjustedYear = year - 1;
  }
  return `${adjustedYear}-${String(month).padStart(2, '0')}-${String(day).padStart(2, '0')}`;
}

function formatParcela(parcela: string): string {
  const [current, total] = parcela.split('/');
  return `Parcela ${parseInt(current, 10)}/${parseInt(total, 10)}`;
}

function shouldIgnoreLine(line: string): boolean {
  if (IGNORED_SECTIONS.some(s => line.includes(s))) return true;
  if (IGNORED_LINE_PATTERNS.some(p => p.test(line))) return true;
  return false;
}

export const santanderPdfParser: InvoiceParser = {
  bankName: 'Santander',

  detect(pages: string[]): boolean {
    const fullText = pages.join(' ').toLowerCase();
    return fullText.includes('banco santander') || fullText.includes('free visa');
  },

  parse(pages: string[]): CardGroup[] {
    const year = inferYear(pages);
    const invoiceMonth = inferInvoiceMonth(pages);
    const cardMap = new Map<string, { holderName: string; expenses: ParsedExpense[] }>();
    let currentCardLabel = '';
    let currentHolderName = '';
    let inPaymentSection = false;
    let inDetailSection = false;

    for (const pageText of pages) {
      const lines = pageText.split('\n');

      for (const rawLine of lines) {
        const line = rawLine.trim();
        if (!line) continue;

        if (line.includes('Detalhamento da Fatura')) {
          inDetailSection = true;
          continue;
        }

        if (!inDetailSection) continue;

        if (line.startsWith('Resumo da Fatura')) {
          inDetailSection = false;
          continue;
        }

        const cardMatch = line.match(CARD_HEADER_REGEX);
        if (cardMatch) {
          const holderName = cardMatch[1].replace(/^@\s*/, '').trim();
          const cardNumber = cardMatch[2].replace(/\s+/g, ' ').trim();
          currentHolderName = holderName;
          currentCardLabel = cardNumber;
          inPaymentSection = false;

          if (!cardMap.has(currentCardLabel)) {
            cardMap.set(currentCardLabel, { holderName: currentHolderName, expenses: [] });
          }
          continue;
        }

        if (!currentCardLabel) continue;

        if (line.startsWith('Pagamento e Demais Créditos')) {
          inPaymentSection = true;
          continue;
        }

        if (line === 'Parcelamentos' || line === 'Despesas') {
          inPaymentSection = false;
          continue;
        }

        if (shouldIgnoreLine(line)) continue;
        if (inPaymentSection) continue;

        const txMatch = line.match(TRANSACTION_REGEX);
        if (!txMatch) continue;

        const [, dayMonth, rawDesc, parcela, valueStr] = txMatch;
        const value = parseValue(valueStr);

        if (value <= 0) continue;

        const description = parcela
          ? `${rawDesc.trim()} - ${formatParcela(parcela)}`
          : rawDesc.trim();
        const date = formatDate(dayMonth, year, invoiceMonth);

        cardMap.get(currentCardLabel)!.expenses.push({ date, description, value });
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
