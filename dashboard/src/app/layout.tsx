import "@/styles/globals.css";

import MapProvider from "@/components/MapProvider";
import { Toaster } from "@/components/ui/sonner";
import { WebSocketProvider } from "@/lib/websocket";
import { GeistSans } from "geist/font/sans";
import type { Metadata } from "next";

export const metadata: Metadata = {
    title: "Flight for life",
    icons: [{ rel: "icon", url: "/favicon.ico" }],
};

export default function RootLayout({ children }: Readonly<{ children: React.ReactNode }>) {
    return (
        <MapProvider>
            <WebSocketProvider>
                <html lang="en" className={`${GeistSans.variable} dark`}>
                    <body className="h-screen flex flex-row">
                        {children}

                        <Toaster position="bottom-left" />
                    </body>
                </html>
            </WebSocketProvider>
        </MapProvider>
    );
}
