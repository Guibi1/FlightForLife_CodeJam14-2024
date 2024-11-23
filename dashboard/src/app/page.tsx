"use client";

import WorldMap from "@/components/WorldMap";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { env } from "@/env";
import type { Drone } from "@/lib/types";
import clsx from "clsx";
import { AnimatePresence, motion } from "framer-motion";
import { CrosshairIcon, Loader2Icon } from "lucide-react";
import { useState } from "react";
import { useMap } from "react-map-gl";

const MotionCard = motion.create(Card);
const MotionCardHeader = motion.create(CardHeader);
const MotionCardContent = motion.create(CardContent);

export default function HomePage() {
    const { map } = useMap();
    const [selectedDrone, selectDrone] = useState<Drone | null>(null);

    const drones: Drone[] = [
        {
            id: 1,
            lng: -122.612707,
            lat: 37.926337,
        },
        {
            id: 2,
            lng: -122.611007,
            lat: 37.926937,
        },
    ];

    return (
        <main className="relative flex-1">
            <div className="absolute inset-0">
                <WorldMap drones={drones} onDroneSelect={selectDrone} />
            </div>

            <Card className="absolute inset-y-8 right-8 w-[30rem] h-fit border border-primary bg-background shadow-2xl">
                <CardHeader>
                    <CardTitle>Flight for life</CardTitle>
                </CardHeader>

                <CardContent>
                    <div className="flex flex-col">
                        {drones.map((drone) => (
                            <div key={drone.id}>
                                <MotionCard
                                    initial={false}
                                    animate={
                                        drone.id === selectedDrone?.id
                                            ? {
                                                  borderWidth: "1px",
                                                  insetInline: "-2rem",
                                                  width: "calc(100% + 4rem)",
                                              }
                                            : { borderWidth: "0px", insetInline: "0rem", width: "100%" }
                                    }
                                    className={clsx(
                                        "relative transition-[background]",
                                        drone.id !== selectedDrone?.id && "hover:bg-muted",
                                    )}
                                >
                                    <MotionCardHeader
                                        className="flex-row items-center justify-between gap-2"
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
                                                "transition-all",
                                                drone.id === selectedDrone?.id && "font-bold",
                                            )}
                                        >
                                            Drone #{drone.id}
                                        </p>

                                        <Button
                                            variant="outline"
                                            size="icon"
                                            onClick={(e) => {
                                                e.stopPropagation();
                                                map?.flyTo({ center: [drone.lng, drone.lat], zoom: 18 });
                                            }}
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
                                                        className="absolute inset-0 text-background"
                                                        src={`${env.NEXT_PUBLIC_SERVER_URL}/drone/${drone.id}`}
                                                        alt="drone video stream"
                                                    />
                                                </div>

                                                <Button size="sm" onClick={() => selectDrone(null)}>
                                                    Back
                                                </Button>
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
