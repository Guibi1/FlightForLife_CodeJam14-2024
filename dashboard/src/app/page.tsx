"use client";
import { useWebSocket } from "@/lib/context/websocketContext";
import Link from "next/link";
import { useState } from "react";

export default function HomePage() {
	const { socket, messages } = useWebSocket();

	const [input, setInput] = useState<string>("");

	const sendMessage = () => {
		if (socket && input.trim()) {
			socket.emit("message", { data: input }); // Emit message to Flask backend
			setInput(""); // Clear input field
		}
	};
	return (
		<main className="flex min-h-screen flex-col items-center justify-center bg-gradient-to-b from-[#2e026d] to-[#15162c] text-white">
			<div className="container flex flex-col items-center justify-center gap-12 px-4 py-16">
				<h1 className="text-5xl font-extrabold tracking-tight text-white sm:text-[5rem]">
					Create <span className="text-[hsl(280,100%,70%)]">T3</span> App
				</h1>
				<div className="grid grid-cols-1 gap-4 sm:grid-cols-2 md:gap-8">
					<Link
						className="flex max-w-xs flex-col gap-4 rounded-xl bg-white/10 p-4 text-white hover:bg-white/20"
						href="https://create.t3.gg/en/usage/first-steps"
						target="_blank"
					>
						<h3 className="text-2xl font-bold">First Steps →</h3>
						<div className="text-lg">
							Just the basics - Everything you need to know to set up your
							database and authentication.
						</div>
					</Link>
					<Link
						className="flex max-w-xs flex-col gap-4 rounded-xl bg-white/10 p-4 text-white hover:bg-white/20"
						href="https://create.t3.gg/en/introduction"
						target="_blank"
					>
						<h3 className="text-2xl font-bold">Documentation →</h3>
						<div className="text-lg">
							Learn more about Create T3 App, the libraries it uses, and how to
							deploy it.
						</div>
					</Link>
				</div>
			</div>
			<div>
				<input
					type="text"
					value={input}
					onChange={(e) => setInput(e.target.value)}
					placeholder="Type your message here"
				/>
				<button type="button" onClick={sendMessage}>Send</button>
                <h1>{input}</h1>
			</div>
		</main>
	);
}
