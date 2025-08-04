import { useEffect } from "react";
import * as signalR from "@microsoft/signalr";

export const useSignalR = (onStatusChanged, onProgressChanged) => {
  useEffect(() => {
    const connection = new signalR.HubConnectionBuilder()
      .withUrl("http://localhost:8285/statusHub")
      .withAutomaticReconnect()
      .build();

    connection.on("StatusChanged", (data) => {
      console.log("📡 Status changed:", data);
      if (onStatusChanged) onStatusChanged(data);
    });

    connection.on("ProgressUpdated", (updatedStudentId) => {
      console.log("📡 Progress updated for:", updatedStudentId);
      if (onProgressChanged) onProgressChanged(updatedStudentId);
    });

    connection.start()
      .then(() => console.log("✅ Connected to SignalR hub"))
      .catch((err) => console.error("SignalR connection error:", err));

    return () => {
      connection.stop();
    };
  }, [onStatusChanged, onProgressChanged]);
};
