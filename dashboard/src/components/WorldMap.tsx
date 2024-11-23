import { env } from "@/env";
import type { Drone } from "@/lib/types";
import Mapbox from "mapbox-gl";
import MapGL, { Layer, Marker, Source, useMap } from "react-map-gl";

import "mapbox-gl/dist/mapbox-gl.css";
import { useEffect, useState } from "react";
import DroneIcon from "./DroneIcon";
import { Button } from "./ui/button";

export default function WorldMap({
    drones,
    onDroneSelect,
}: { drones: Drone[]; onDroneSelect: (drone: Drone) => void }) {
    const [styleLoaded, setStyleLoaded] = useState(false);
    const { map } = useMap();

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
                longitude: -122.612707,
                latitude: 37.926337,
                zoom: 16,
                pitch: 20,
            }}
            mapStyle="mapbox://styles/guibi/cm3tlr9vo00hc01rwbr7ga0us"
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
                        onClick={() => {
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
                                "circle-color": "rgba(124,58,237,0.2)",
                                "circle-stroke-color": "rgb(124,58,237)",
                                "circle-stroke-width": 1,
                            }}
                        />
                    </Source>
                ))}
        </MapGL>
    );
}
