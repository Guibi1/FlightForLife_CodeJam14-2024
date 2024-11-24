from openai import OpenAI
from dotenv import load_dotenv
import os
from twilio.rest import Client
import json
import base64
import requests

load_dotenv()
#
OPENAI_SECRET_KEY = os.getenv("OPENAI_SECRET_KEY")
TWILIO_ACCOUNT_SID = os.getenv("TWILIO_ACCOUNT_SID")
TWILIO_AUTH_KEY = os.getenv("TWILIO_AUTH_KEY")
key_imgbb = os.getenv("key_imgbb")

client = OpenAI(api_key=OPENAI_SECRET_KEY)
twilio_client = Client(TWILIO_ACCOUNT_SID, TWILIO_AUTH_KEY)


def getGPTResponseToHelper(base64_image):
    response = client.chat.completions.create(
        model="gpt-4o-mini",
        messages=[
            {
                "role": "system",
                "content": (
                    "You are a first responder analysis assistant. Your role is to assess individual humans within the green boxes in the provided image. "
                    "Identify their physical and mental needs, focusing only on their condition. If the environment appears dangerous, include a clear notice about the potential hazard. "
                    "Keep your responses concise, precise, and focused solely on the individuals in the green box. This is not a conversation. Only provide factual informations. Do not hallucinate."
                ),
            },
            {
                "role": "user",
                "content": [
                    {
                        "type": "text",
                        "text": "What is in this image?",
                    },
                    {
                        "type": "image_url",
                        "image_url": {"url": f"data:image/jpeg;base64,{base64_image}"},
                    },
                ],
            },
        ],
    )

    sendSMSwithTwilio(response.choices[0].message.content, base64_image)


def sendSMSwithTwilio(response, base64_image):
    url = "https://api.imgbb.com/1/upload"
    payload = {
        "key": key_imgbb,
        "image": base64_image,
    }
    res = requests.post(url, payload)

    message = twilio_client.messages.create(
        from_="+19788296846",
        body=f"{response}",
        to="+14383890928",
        media_url=[res.json()["data"]["url"]],
    )

    print(message)
