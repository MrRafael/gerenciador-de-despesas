export interface ParsedExpense {
  date: string;
  description: string;
  value: number;
}

export interface CardGroup {
  cardLabel: string;
  holderName: string;
  expenses: ParsedExpense[];
  totalValue: number;
}

export interface InvoiceParser {
  bankName: string;
  detect(pages: string[]): boolean;
  parse(pages: string[]): CardGroup[];
}
