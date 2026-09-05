import { useEffect } from "react";
import { setDocumentTitle } from "@/utils/documentTitle";

interface PlaceholderPageProps {
  title: string;
  message: string;
}

export function PlaceholderPage({ title, message }: PlaceholderPageProps) {
  useEffect(() => {
    setDocumentTitle(title);
  }, [title]);

  return <div className="text-gray-500">{message}</div>;
}
