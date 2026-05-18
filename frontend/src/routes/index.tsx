import { MainLayout } from "@/components/layout/MainLayout.tsx";
import { NotFoundPage } from "@/pages/NotFoundPage.tsx";
import { createBrowserRouter } from "react-router-dom";


const router = createBrowserRouter([
  {
    element: <MainLayout />,
    children: [
      {
        index: true,
        element: (
          <div className="text-gray-500">Dashboard — coming in Sprint 4</div>
        ),
      },
      {
        path: "transactions",
        element: (
          <div className="text-gray-500">Transactions — coming in Sprint 2</div>
        ),
      },
      {
        path: "categories",
        element: (
          <div className="text-gray-500">Categories — coming in Sprint 2</div>
        ),
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
]);

export default router;