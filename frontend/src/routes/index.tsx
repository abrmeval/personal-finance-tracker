import { createBrowserRouter } from "react-router-dom";
import { MainLayout } from "@/components/layout/MainLayout";
import { ProtectedRoute } from "@/features/auth/ProtectedRoute";
import { NotFoundPage } from "@/pages/NotFoundPage";
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
              <div className="text-gray-500">
                Dashboard — coming in Sprint 4
              </div>
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
              <div className="text-gray-500">Budgets — coming in Sprint 3</div>
            ),
          },
          {
            path: "reports",
            element: (
              <div className="text-gray-500">Reports — coming in Sprint 4</div>
            ),
          },
          { path: "*", element: <NotFoundPage /> },
        ],
      },
    ],
  },
]);
export default router;
