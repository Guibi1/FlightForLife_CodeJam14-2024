"use client";

import WorldMap from "@/components/WorldMap";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { env } from "@/env";
import type { Drone } from "@/lib/types";
import { useWebSocket } from "@/lib/websocket";
import clsx from "clsx";
import { AnimatePresence, motion } from "framer-motion";
import { CrosshairIcon, EyeClosedIcon, LifeBuoyIcon, Loader2Icon, TriangleAlertIcon } from "lucide-react";
import { useState } from "react";
import { useMap } from "react-map-gl";

const MotionCard = motion.create(Card);
const MotionCardHeader = motion.create(CardHeader);
const MotionCardContent = motion.create(CardContent);

export default function HomePage() {
    const { map } = useMap();
    const { drones, send } = useWebSocket();
    const [selectedDrone, selectDrone] = useState<Drone | null>(null);

    return (
        <main className="relative flex-1">
            <div className="absolute inset-0">
                <WorldMap onDroneSelect={selectDrone} />
            </div>

            <Card className="absolute inset-y-8 right-8 w-[40rem] h-fit border border-primary bg-background shadow-2xl">
                <CardHeader>
                    <CardTitle className={clsx("transition-opacity", selectedDrone && "opacity-55")}>
                        Flight for life
                    </CardTitle>
                </CardHeader>

                <CardContent>
                    <div className="divide-y flex flex-col">
                        {drones.map((drone) => (
                            <div key={drone.id}>
                                <MotionCard
                                    initial={false}
                                    animate={
                                        drone.id === selectedDrone?.id
                                            ? {
                                                  borderWidth: "1px",
                                                  left: "-1rem",
                                                  width: "calc(100% + 2rem)",
                                              }
                                            : { borderWidth: "0px", left: "0rem", width: "100%" }
                                    }
                                    className={clsx(
                                        "relative transition-opacity rounded my-1",
                                        selectedDrone && drone.id !== selectedDrone.id && "opacity-55",
                                    )}
                                >
                                    <MotionCardHeader
                                        className={clsx(
                                            "flex-row items-center gap-2 transition-[background]",
                                            drone.id !== selectedDrone?.id && "hover:bg-muted",
                                        )}
                                        onClick={() => selectDrone(drone)}
                                        onKeyDown={(e: KeyboardEvent) => {
                                            if (e.key === "Enter") selectDrone(drone);
                                        }}
                                        tabIndex={0}
                                        initial={false}
                                        animate={
                                            drone.id === selectedDrone?.id
                                                ? { paddingInline: "1.5rem", paddingBlock: "1.5rem" }
                                                : { paddingInline: "1rem", paddingBlock: ".5rem" }
                                        }
                                    >
                                        <p
                                            className={clsx(
                                                "transition-all mr-auto",
                                                drone.id === selectedDrone?.id && "font-bold",
                                            )}
                                        >
                                            Drone #{drone.id}
                                        </p>

                                        {drone.alert && (
                                            <Button variant="destructive" size="icon" className="relative">
                                                <div className="absolute inset-0 bg-destructive animate-ping duration-1000" />
                                                <TriangleAlertIcon />
                                            </Button>
                                        )}

                                        <Button
                                            variant="outline"
                                            size="icon"
                                            onClick={() =>
                                                map?.flyTo({
                                                    center: [drone.lng, drone.lat],
                                                    padding: { right: 350 },
                                                    zoom: 18,
                                                })
                                            }
                                        >
                                            <CrosshairIcon />
                                        </Button>
                                    </MotionCardHeader>

                                    <AnimatePresence>
                                        {drone.id === selectedDrone?.id && (
                                            <MotionCardContent
                                                initial={{ height: 0, opacity: 0, marginBottom: "0rem" }}
                                                animate={{ height: "auto", opacity: 1, marginBottom: "1.5rem" }}
                                                exit={{ height: 0, opacity: 0, marginBottom: "0rem" }}
                                                className="overflow-hidden pb-0"
                                            >
                                                <div className="relative h-64 grid place-items-center overflow-hidden rounded mb-4">
                                                    <Loader2Icon className="animate-spin" />

                                                    <img
                                                        className="absolute inset-0"
                                                        src={`${env.NEXT_PUBLIC_SERVER_URL}/drone/${drone.id}`}
                                                        alt="drone video stream"
                                                    />
                                                </div>

                                                <div className="flex gap-2">
                                                    <Button
                                                        variant={drone.alert ? "outline" : "default"}
                                                        className="mr-auto"
                                                        onClick={() => {
                                                            selectDrone(null);
                                                            if (map && map.getZoom() >= 18) map?.zoomTo(16.5);
                                                        }}
                                                    >
                                                        Back
                                                    </Button>

                                                    {drone.alert && (
                                                        <Button
                                                            onClick={() =>
                                                                send({
                                                                    type: "dismiss_alert",
                                                                    drone: drone.id,
                                                                    confirmed: true,
                                                                })
                                                            }
                                                        >
                                                            <LifeBuoyIcon />
                                                            Send help
                                                        </Button>
                                                    )}

                                                    {drone.alert && (
                                                        <Button
                                                            variant="destructive"
                                                            onClick={() =>
                                                                send({
                                                                    type: "dismiss_alert",
                                                                    drone: drone.id,
                                                                    confirmed: false,
                                                                })
                                                            }
                                                        >
                                                            <EyeClosedIcon />
                                                            Ignore
                                                        </Button>
                                                    )}
                                                </div>
                                            </MotionCardContent>
                                        )}
                                    </AnimatePresence>
                                </MotionCard>
                            </div>
                        ))}
                    </div>
                </CardContent>
            </Card>
        </main>
    );
}
