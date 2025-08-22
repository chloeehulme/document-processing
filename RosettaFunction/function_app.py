import azure.functions as func
from azure.core.credentials import AzureKeyCredential
from azure.ai.formrecognizer import DocumentAnalysisClient
from dotenv import load_dotenv
import os
import datetime
import json
import logging

load_dotenv()
app = func.FunctionApp()

endpoint = os.environ["FORM_RECOGNIZER_ENDPOINT"]
key = os.environ["FORM_RECOGNIZER_KEY"]

document_analysis_client = DocumentAnalysisClient(
    endpoint=endpoint, credential=AzureKeyCredential(key)
)

@app.queue_trigger(arg_name="azqueue", queue_name="insert queue name",
                               connection="insert connection string") 
def docprocessqueue(azqueue: func.QueueMessage):
    message_body = azqueue.get_body().decode('utf-8')

    logging.info('Python Queue trigger processed a message: %s',
                message_body)

    try:
        formUrl = json.loads(message_body)["BlobUri"]
        
        poller = document_analysis_client.begin_analyze_document_from_url("prebuilt-document", formUrl)
        result = poller.result()

        print("----Key-value pairs found in document----")
        for kv_pair in result.key_value_pairs:
            if kv_pair.key and kv_pair.value:
                print("Key '{}': Value: '{}'".format(kv_pair.key.content, kv_pair.value.content))
            else:
                print("Key '{}': Value:".format(kv_pair.key.content))

        print("----------------------------------------")
    
    except Exception as e:
        logging.error(f"Failed to process document: {e}")
