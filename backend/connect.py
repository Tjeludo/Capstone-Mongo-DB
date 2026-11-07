import os
from pymongo import MongoClient, errors
from dotenv import load_dotenv, find_dotenv

load_dotenv(find_dotenv())
MONGO_PASSWD = os.environ.get("mongodb_passwd")
MONGO_STR = f"mongodb+srv://ava:{MONGO_PASSWD}@capstone-mongodb.x8z40vh.mongodb.net/?retryWrites=true&w=majority&appName=capstone-mongodb"

# Complete
def connect_to_mongodb(table_name):
    try:
        client = MongoClient(MONGO_STR)
        database = client.sample_mflix
        table = database[table_name]
        return table
    except errors.ConnectionFailure as e:
        print(f"Connection failed: {e}")
        return {"success": False, "message": f"Failed to connect to MongoDB table '{table_name}': {e}"}

def view_databases():
    try:
        client = MongoClient(MONGO_STR)
        dbs = client.list_database_names()
        print(f"Databases: {dbs}")
        return {"success": True, "databases": dbs, "message": "Successfully retrieved database list."}
    except Exception as err:
        print(f"Failed to retrieve databases: {err}")
        return {"success": False, "message": f"Failed to retrieve databases: {err}"}

def view_tables():
    try:
        client = MongoClient(MONGO_STR)
        database = client.sample_mflix
        collections = database.list_collection_names()
        print(f"Collections: {collections}")
        return {"success": True, "collections": collections, "message": "Successfully retrieved collection list."}
    except Exception as err:
        print(f"Failed to retrieve collections: {err}")
        return {"success": False, "message": f"Failed to retrieve collections: {err}"}
