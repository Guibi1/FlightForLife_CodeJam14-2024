"use client"
import { env } from "@/env";
import type React from "react";
import type { ReactNode } from "react";
import { createContext, useContext, useEffect, useState } from "react";
import { io } from "socket.io-client";
import type { Socket } from "socket.io-client";

// Define the shape of the WebSocket context
interface WebSocketContextType {
	socket: Socket | null; // Socket instance
	messages: string[]; // List of messages from the server
}

// Create the WebSocket context
const WebSocketContext = createContext<WebSocketContextType | undefined>(
	undefined,
);

// Define the WebSocket provider props
interface WebSocketProviderProps {
	children: ReactNode;
}

// WebSocket Provider component
export const WebSocketProvider: React.FC<WebSocketProviderProps> = ({
	children,
}) => {
	const [socket, setSocket] = useState<Socket | null>(null);
	const [messages, setMessages] = useState<string[]>([]);

	useEffect(() => {
		// Initialize the Socket.IO connection
		const socketInstance = io(env.NEXT_PUBLIC_SERVER_URL); // Flask server URL
		setSocket(socketInstance);

		// Listen for "response" events from the server
		socketInstance.on("response", (data: { data: string }) => {
			console.log("Message from server:", data.data);
			setMessages((prevMessages) => [...prevMessages, data.data]);
		});

		// Cleanup when component unmounts
		return () => {
			socketInstance.disconnect();
		};
	}, []);

	return (
		<WebSocketContext.Provider value={{ socket, messages }}>
			{children}
		</WebSocketContext.Provider>
	);
};

// Custom hook for consuming the WebSocket context
export const useWebSocket = (): WebSocketContextType => {
	const context = useContext(WebSocketContext);
	if (!context) {
		throw new Error("useWebSocket must be used within a WebSocketProvider");
	}
	return context;
};
