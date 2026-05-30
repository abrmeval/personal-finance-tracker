/**
 * Sets the document title based on the provided title. If no title is provided, it defaults to "Personal Finance Tracker".
 * @param title The title to set for the document.
 */
export function setDocumentTitle(title?: string) {
    const template = title ? `${title} | Personal Finance Tracker` : "Personal Finance Tracker";
    document.title = template;
}