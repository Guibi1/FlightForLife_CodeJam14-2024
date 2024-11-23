"use client";
import { env } from "@/env";
import type { ReactNode } from "react";
import { createContext, useContext, useEffect, useMemo, useState } from "react";
import { type Socket, io } from "socket.io-client";
import type { Drone } from "./types";

type Message =
	| { type: "dismiss-alert"; drone: number; confirmed: boolean }
	| { type: "request-movement"; lng: number; lat: number };
type WebSocketContextType = {
	send: (message: Message) => void;
	drones: Drone[];
};

const context = createContext<WebSocketContextType | null>(null);

export function WebSocketProvider({ children }: { children: ReactNode }) {
	const [socket, setSocket] = useState<Socket | null>(null);
	const [drones, setDrones] = useState<Drone[]>([
		{
			id: 1,
			lng: -122.612707,
			lat: 37.926337,
			alert: false,
		},
		{
			id: 2,
			lng: -122.611007,
			lat: 37.926937,
			alert: true,
		},
	]);

	useEffect(() => {
		const socketInstance = io(`${env.NEXT_PUBLIC_SERVER_URL}/frontend`);
		setSocket(socketInstance);

		socketInstance.on("drones", (drones: string) => {
			console.log("Message from server:", drones);
			setDrones(JSON.parse(drones));
		});

		return () => {
			socketInstance.disconnect();
			setSocket(null);
		};
	}, []);

	const send = useMemo(
		() => (message: Message) => socket?.emit(message.type, message),
		[socket],
	);
	return (
		<context.Provider value={{ drones, send }}>{children}</context.Provider>
	);
}

export function useWebSocket(): WebSocketContextType {
	const c = useContext(context);
	if (!c) {
		throw new Error("useWebSocket must be used within a WebSocketProvider");
	}
	return c;
}
