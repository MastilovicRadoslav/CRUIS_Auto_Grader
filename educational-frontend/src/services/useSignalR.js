import { useEffect } from "react";
import * as signalR from "@microsoft/signalr";

export const useSignalR = (onStatusChanged, onProgressChanged, onStudentPurged) => {
  useEffect(() => {
    const connection = new signalR.HubConnectionBuilder()
      .withUrl("http://localhost:8285/statusHub")
      .withAutomaticReconnect()
      .build();

    const handleStatusChanged = (data) => {
      console.log("📡 Status changed:", data);
      if (typeof onStatusChanged === "function") onStatusChanged(data);
    };

    const handleProgressUpdated = (updatedStudentId) => {
      console.log("📡 Progress updated for:", updatedStudentId);
      if (typeof onProgressChanged === "function") onProgressChanged(updatedStudentId);
    };

    const handleStudentPurged = (payload) => {
      const studentId = payload && payload.studentId ? payload.studentId : payload;
      console.log("🧹 Student purged:", studentId, payload);
      if (typeof onStudentPurged === "function") onStudentPurged(studentId);
    };

    connection.on("StatusChanged", handleStatusChanged);
    connection.on("ProgressUpdated", handleProgressUpdated);
    connection.on("StudentPurged", handleStudentPurged);

    connection
      .start()
      .then(() => console.log("✅ Connected to SignalR hub"))
      .catch((err) => console.error("SignalR connection error:", err));

    return () => {
      connection.off("StatusChanged", handleStatusChanged);
      connection.off("ProgressUpdated", handleProgressUpdated);
      connection.off("StudentPurged", handleStudentPurged);
      connection.stop();
    };
  }, [onStatusChanged, onProgressChanged, onStudentPurged]);
};
