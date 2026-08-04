/** Demo so'rovi — hali tenant emas, faqat bog'lanish uchun ma'lumot. */
export interface CreateLeadDto {
  companyName: string;
  contactName: string;
  phone: string;
  email?: string | null;
  note?: string | null;
}
