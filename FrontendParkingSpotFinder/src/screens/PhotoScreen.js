import React from "react";
import { Image, Text, StyleSheet } from "react-native";
import { LinearGradient } from "expo-linear-gradient";

export default function PhotoScreen({ route }) {
  const { imageUrl, timestamp } = route.params;

  const date = new Date(timestamp);
  const formattedDate = date.toLocaleDateString();
  const formattedTime = date.toLocaleTimeString([], {
    hour: "2-digit",
    minute: "2-digit",
  });

  return (
    <LinearGradient
      colors={["#b993d6", "#8ca6db"]}
      start={{ x: 0, y: 0 }}
      end={{ x: 0, y: 1 }}
      style={styles.container}
    >
      <Image source={{ uri: imageUrl }} style={styles.image} />
      <Text style={styles.timestamp}>
        Dodano: {formattedDate} o {formattedTime}
      </Text>
    </LinearGradient>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    padding: 16,
    alignItems: "center",
    justifyContent: "center",
  },
  image: {
    width: "100%",
    height: 400,
    resizeMode: "contain",
    marginBottom: 8,
  },
  timestamp: {
    color: "#fff",
    fontSize: 14,
    fontFamily: "PressStart2P",
  },
});
