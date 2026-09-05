import { createBrowserRouter } from "react-router-dom";
import { MainLayout } from "@/components/layout/MainLayout";
import { ProtectedRoute } from "@/features/auth/ProtectedRoute";
import { NotFoundPage } from "@/pages/NotFoundPage";
import { PlaceholderPage } from "@/pages/PlaceholderPage";
import { LoginPage } from "@/features/auth/LoginPage";
import { RegisterPage } from "@/features/auth/RegisterPage";
import { TransactionsPage } from "@/features/transactions/pages/TransactionsPage";
import { CategoriesPage } from "@/features/categories/pages/CategoriesPage";

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
            element: (
              <PlaceholderPage
                title="Dashboard"
                message="Dashboard — coming in Sprint 4"
              />
            ),
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
            element: (
              <PlaceholderPage
                title="Budgets"
                message="Budgets — coming in Sprint 3"
              />
            ),
          },
          {
            path: "reports",
            element: (
              <PlaceholderPage
                title="Reports"
                message="Reports — coming in Sprint 4"
              />
            ),
          },
          { path: "*", element: <NotFoundPage /> },
        ],
      },
    ],
  },
]);
export default router;
