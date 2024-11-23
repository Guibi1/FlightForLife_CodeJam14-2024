import { env } from "@/env";
import type { Drone } from "@/lib/types";
import { useWebSocket } from "@/lib/websocket";
import { FlagIcon } from "lucide-react";
import Mapbox, { type LngLat } from "mapbox-gl";
import "mapbox-gl/dist/mapbox-gl.css";
import { nanoid } from "nanoid";
import { useEffect, useState } from "react";
import MapGL, { Layer, Marker, Source, useMap } from "react-map-gl";
import { toast } from "sonner";
import DroneIcon from "./DroneIcon";
import { Button } from "./ui/button";

export default function WorldMap({ onDroneSelect }: { onDroneSelect: (drone: Drone) => void }) {
    const { drones, send } = useWebSocket();
    const { map } = useMap();
    const [styleLoaded, setStyleLoaded] = useState(false);
    const [pointClicked, setPointClicked] = useState<LngLat | null>(null);

    useEffect(() => {
        setStyleLoaded(map?.isStyleLoaded() ?? false);
        if (!map) return;
        const hop = () => setStyleLoaded(true);
        map.on("style.load", hop);
        return () => {
            map.off("styledata", hop);
        };
    }, [map]);

    return (
        <MapGL
            // @ts-ignore Mapbox package version
            mapLib={Mapbox}
            mapboxAccessToken={env.NEXT_PUBLIC_MAPBOX_API}
            id="map"
            initialViewState={{
                longitude: -0.375984,
                latitude: 39.47132,
                zoom: 16,
                pitch: 60,
            }}
            mapStyle="mapbox://styles/guibi/cm3tlr9vo00hc01rwbr7ga0us"
            onClick={(e) => setPointClicked(e.lngLat)}
        >
            {drones.map((drone) => (
                <Marker
                    longitude={drone.lng}
                    latitude={drone.lat}
                    anchor="center"
                    rotationAlignment="viewport"
                    key={drone.id}
                >
                    <Button
                        size="icon"
                        variant="secondary"
                        onClick={(e) => {
                            e.stopPropagation();
                            setPointClicked(null);
                            onDroneSelect(drone);
                            map?.flyTo({ center: [drone.lng, drone.lat] });
                        }}
                        className="z-10"
                    >
                        <DroneIcon primary="fill-primary" secondary="fill-foreground/40" />
                    </Button>
                </Marker>
            ))}

            {styleLoaded &&
                drones.map((drone) => (
                    <Source
                        type="geojson"
                        data={{
                            type: "FeatureCollection",
                            features: [
                                {
                                    type: "Feature",
                                    geometry: {
                                        type: "Point",
                                        coordinates: [drone.lng, drone.lat],
                                    },
                                },
                            ],
                        }}
                        key={drone.id}
                    >
                        <Layer
                            type="circle"
                            paint={{
                                "circle-radius": {
                                    stops: [
                                        [0, 0],
                                        [20, 800],
                                    ],
                                    base: 2,
                                },
                                "circle-color": drone.alert ? "rgba(127,29,29,0.2)" : "rgba(34,197,94,0.2)",
                                "circle-stroke-color": drone.alert ? "rgb(127,29,29)" : "rgb(34,197,94)",
                                "circle-stroke-width": 1,
                            }}
                        />
                    </Source>
                ))}

            {pointClicked && (
                <Marker
                    longitude={pointClicked.lng}
                    latitude={pointClicked.lat}
                    anchor="center"
                    rotationAlignment="viewport"
                >
                    <Button
                        size="icon"
                        className="z-10 rounded-full"
                        onClick={(e) => {
                            e.stopPropagation();
                            const id = nanoid();
                            send({ type: "request_movement", ...pointClicked, id });
                            toast.success("A drone is being sent to this location", {
                                cancel: {
                                    label: "Abort",
                                    onClick: () => send({ type: "abort_movement", id }),
                                },
                            });
                            setPointClicked(null);
                        }}
                    >
                        <FlagIcon />
                    </Button>
                </Marker>
            )}
        </MapGL>
    );
}
