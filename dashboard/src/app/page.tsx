"use client";

import WorldMap from "@/components/WorldMap";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import type { Drone } from "@/lib/types";
import { CrosshairIcon } from "lucide-react";
import { useState } from "react";
import { useMap } from "react-map-gl";

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

            <Card className="absolute inset-y-8 right-8 w-[30rem] h-fit border border-primary bg-background">
                <CardHeader>
                    <CardTitle>Flight for life</CardTitle>
                </CardHeader>

                <CardContent>
                    <div className="divide-y flex flex-col">
                        {drones.map((drone) => (
                            <div
                                className="py-2 px-4 flex items-center justify-between rounded hover:bg-muted transition-[background]"
                                onClick={() => selectDrone(drone)}
                                onKeyDown={(e) => {
                                    if (e.key === "Enter") selectDrone(drone);
                                }}
                                // biome-ignore lint/a11y/noNoninteractiveTabindex: I have a on keydown
                                tabIndex={0}
                                key={drone.id}
                            >
                                <p>Drone #{drone.id}</p>

                                {selectedDrone?.id === drone.id && "SELECTED"}

                                <Button
                                    variant="outline"
                                    size="icon"
                                    onClick={() => map?.flyTo({ center: [drone.lng, drone.lat], zoom: 18 })}
                                >
                                    <CrosshairIcon />
                                </Button>
                            </div>
                        ))}
                    </div>
                </CardContent>
            </Card>
        </main>
    );
}
