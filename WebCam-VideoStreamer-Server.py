import asyncio
import websockets
import cv2

async def stream_webcam(websocket, path):
    print("Client connecté.")
    cap = cv2.VideoCapture(0)  # 0 = caméra intégrée par défaut

    try:
        while cap.isOpened():
            ret, frame = cap.read()
            if not ret:
                print("Impossible de lire la webcam.")
                break

            # Encode en JPEG
            _, buffer = cv2.imencode('.jpg', frame)

            # Envoie la frame encodée via WebSocket
            await websocket.send(buffer.tobytes())

            await asyncio.sleep(0.05)  # ~20 FPS (ajuste si besoin)
    except websockets.ConnectionClosed:
        print("Connexion WebSocket fermée par le client.")
    finally:
        cap.release()

start_server = websockets.serve(stream_webcam, "0.0.0.0", 8765, ping_interval=None)

print("Serveur de webcam lancé sur ws://localhost:8765")
asyncio.get_event_loop().run_until_complete(start_server)
asyncio.get_event_loop().run_forever()
