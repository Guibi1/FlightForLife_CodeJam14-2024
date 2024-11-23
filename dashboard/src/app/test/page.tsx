"use client"
import type React from "react";
import { useState } from "react";
import { useWebSocket } from "@lib/websocket";

const WebSocketInput: React.FC = () => {
	const { socket } = useWebSocket(); // Access the WebSocket from the context
	const [input, setInput] = useState<string>(""); // State for the input field

	const handleInputKeyPress = () => {
		if (socket && input.trim()) {
			socket.emit("message", { data: input }); // Send the input to the WebSocket server
			setInput(""); // Clear the input field after sending
		}
	};

	return (
		<>
			<input
				type="text"
				value={input}
				onChange={(e) => setInput(e.target.value)} // Update state as the user types
				placeholder="Type a message and press Enter"
			/>
			<button type="button" onClick={handleInputKeyPress}>Send</button>
		</>
	);
};

export default WebSocketInput;
