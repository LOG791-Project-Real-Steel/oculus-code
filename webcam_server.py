import asyncio  
import websockets
import cv2

async def stream_webcam(websocket):
    print("📷 Connexion entrante – webcam streaming lancé...")
    cap = cv2.VideoCapture(0, cv2.CAP_DSHOW)

    try:
        while cap.isOpened():
            ret, frame = cap.read()
            if not ret:
                print("❌ Erreur de lecture webcam.")
                break

            success, buffer = cv2.imencode('.jpg', frame)
            if not success:
                print("❌ Erreur d'encodage JPEG.")
                continue

            await websocket.send(buffer.tobytes())
            await asyncio.sleep(0.05)

    except websockets.ConnectionClosed:
        print("🔌 Connexion WebSocket fermée.")
    finally:
        cap.release()
        print("🛑 Webcam relâchée.")

async def main():
    print("🚀 Lancement du serveur WebSocket...")
    async with websockets.serve(stream_webcam, "0.0.0.0", 8765, ping_interval=None):
        print("✅ Serveur en ligne sur ws://localhost:8765")
        await asyncio.Future()  # Pour garder le serveur actif

if __name__ == "__main__":
    asyncio.run(main())
