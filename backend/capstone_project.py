from fastapi import FastAPI
from .branch_api import  router as branch_router
from .category_api import router as category_router
from .product_api import router as product_router
from .order_api import router as order_router
from .supplier_api import router as supplier_router
from .supplier_products_api import router as supplier_product_router
from .staff_api import router as staff_router
from .user_api import router as user_router
from .roles_api import router as role_router
from .permissions_api import router as permissions_router
from .analytics_api import router as analytics_router
from .InventoryAPI import  router as InventoryAPI_router
from .api import router as api_router
from .home_api import  router as home_router
from .message_request_api import router as message_request_router

app = FastAPI()

app.include_router(user_router)
app.include_router(category_router)
app.include_router(product_router)
app.include_router(supplier_router)
app.include_router(staff_router)
app.include_router(role_router)
app.include_router(branch_router)
app.include_router(order_router)
app.include_router(supplier_product_router)
app.include_router(permissions_router)
app.include_router(analytics_router)
app.include_router(InventoryAPI_router)
app.include_router(api_router)
app.include_router(home_router)
app.include_router(message_request_router)