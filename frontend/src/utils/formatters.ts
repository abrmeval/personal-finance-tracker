export function formatCurrency(amount: number): string {
  return new Intl.NumberFormat("es-MX", {
    style: "currency",
    currency: "MXN",
  }).format(amount);
}

export function formatDate(dateString: string): string {
  return new Date(`${dateString.slice(0, 10)}T00:00:00`).toLocaleDateString(
    "es-MX",
    {
      year: "numeric",
      month: "short",
      day: "numeric",
    },
  );
}
