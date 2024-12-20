"use client";
import { env } from "@/env";
import type { ReactNode } from "react";
import { createContext, useContext, useEffect, useMemo, useState } from "react";
import { type Socket, io } from "socket.io-client";
import type { Drone } from "./types";

type Message =
	| { type: "dismiss_alert"; drone: number; confirmed: boolean }
	| { type: "stop_override"; drone: number }
	| { type: "request_movement"; lng: number; lat: number; id: string }
	| { type: "abort_movement"; id: string };
type WebSocketContextType = {
	send: (message: Message) => void;
	drones: Drone[];
};

const context = createContext<WebSocketContextType | null>(null);

export function WebSocketProvider({ children }: { children: ReactNode }) {
	const [socket, setSocket] = useState<Socket | null>(null);
	const [drones, setDrones] = useState<Drone[]>([]);

	useEffect(() => {
		const socketInstance = io(`${env.NEXT_PUBLIC_SERVER_URL}/frontend`);
		setSocket(socketInstance);

		socketInstance.on("drones", (drones: Drone[]) => {
			console.log("Message from server:", drones);
			setDrones(drones);
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
