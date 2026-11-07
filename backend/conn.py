from dotenv import load_dotenv, find_dotenv
import os
import pprint
from pymongo import MongoClient

def connect():
    """Connect to Mongo db """
    load_dotenv(find_dotenv())     # finds the .env file 
    password = os.environ.get("mongodb_passwd")    # finds the variable "MONGO_PASSWORD"
    connection_string = f"mongodb+srv://ava:{password}@capstone-mongodb.x8z40vh.mongodb.net/?retryWrites=true&w=majority&appName=capstone-mongodb"
    client = MongoClient(connection_string)
    dbs = client.list_database_names()
    database = client["sample_mflix"]
    collection = database["Branch"]
    currentCount = collection.count_documents({})
    #print(currentCount)
    #print(dbs)
    return client

#connect()

