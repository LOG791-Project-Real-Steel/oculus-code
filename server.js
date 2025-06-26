const WebSocket = require("ws");

const wss = new WebSocket.Server({ port: 8888 });

let unityClient = null;
let robotClient = null;

wss.on("connection", ws => {
  ws.on("message", message => {
    const data = JSON.parse(message);

    if (data.role === "unity") {
      unityClient = ws;
      console.log("Unity connected");
    } else if (data.role === "robot") {
      robotClient = ws;
      console.log("Robot connected");
    }

    // Relay messages between robot and Unity
    if (data.to === "robot" && robotClient) {
      robotClient.send(JSON.stringify(data));
    } else if (data.to === "unity" && unityClient) {
      unityClient.send(JSON.stringify(data));
    }
  });

  ws.on("close", () => {
    if (ws === unityClient) unityClient = null;
    if (ws === robotClient) robotClient = null;
  });
});

console.log("Signaling server running on ws://localhost:8888");
