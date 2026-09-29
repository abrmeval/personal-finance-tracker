import { createBrowserRouter } from "react-router-dom";
import { MainLayout } from "@/components/layout/MainLayout";
import { ProtectedRoute } from "@/features/auth/ProtectedRoute";
import { NotFoundPage } from "@/pages/NotFoundPage";
import { DashboardPage } from "@/features/dashboard/pages/DashboardPage";
import { ReportsPage } from "@/features/reports/pages/ReportsPage";
import { LoginPage } from "@/features/auth/LoginPage";
import { RegisterPage } from "@/features/auth/RegisterPage";
import { TransactionsPage } from "@/features/transactions/pages/TransactionsPage";
import { CategoriesPage } from "@/features/categories/pages/CategoriesPage";
import { BudgetsPage } from "@/features/budgets/pages/BudgetsPage";

const router = createBrowserRouter([
  {
    path: "/login",
    element: <LoginPage />,
  },
  {
    path: "/register",
    element: <RegisterPage />,
  },
  {
    element: <ProtectedRoute />,
    children: [
      {
        element: <MainLayout />,
        children: [
          {
            index: true,
            element: <DashboardPage />,
          },
          {
            path: "transactions",
            element: <TransactionsPage />,
          },
          {
            path: "categories",
            element: <CategoriesPage />,
          },
          {
            path: "budgets",
            element: <BudgetsPage />,
          },
          {
            path: "reports",
            element: <ReportsPage />,
          },
          { path: "*", element: <NotFoundPage /> },
        ],
      },
    ],
  },
]);
export default router;
