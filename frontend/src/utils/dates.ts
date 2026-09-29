import { endOfMonth, format, startOfMonth } from "date-fns";

export interface DateRange {
  startDate: string;
  endDate: string;
}

export function getCurrentMonthRange(): DateRange {
  const now = new Date();
  return {
    startDate: format(startOfMonth(now), "yyyy-MM-dd"),
    endDate: format(endOfMonth(now), "yyyy-MM-dd"),
  };
}
