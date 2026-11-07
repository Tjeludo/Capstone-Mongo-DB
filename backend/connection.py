import os
from pymongo import MongoClient, errors
from dotenv import load_dotenv, find_dotenv

load_dotenv(find_dotenv())
MONGO_PASSWD = os.environ.get("mongodb_passwd")
MONGO_STR = f"mongodb+srv://ava:{MONGO_PASSWD}@capstone-mongodb.x8z40vh.mongodb.net/?retryWrites=true&w=majority&appName=capstone-mongodb"

# Complete
def connect_MongoDB(tablename):
    try:
        client = MongoClient(MONGO_STR)
        database = client.sample_mflix
        table = database[tablename]
        return table
    except errors.ConnectionFailure as e:
        print(f"Connection failed: {e}")

#! in progress 
def view_databases():
    pass

def view_tables():
    pass
