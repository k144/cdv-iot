import React from "react";
import { TouchableOpacity, Text, StyleSheet } from "react-native";

export default function PixelButton({ label, onPress }) {
  return (
    <TouchableOpacity
      style={styles.button}
      onPress={onPress}
      activeOpacity={0.8}
    >
      <Text style={styles.text}>{label}</Text>
    </TouchableOpacity>
  );
}

const styles = StyleSheet.create({
  button: {
    backgroundColor: "#ffe4fa",
    paddingVertical: 14,
    paddingHorizontal: 32,
    borderRadius: 30,
    marginBottom: 24,
    borderWidth: 2,
    borderColor: "#6e3bed",
    alignItems: "center",
  },
  text: {
    fontFamily: "PressStart2P",
    fontSize: 15,
    color: "#6e3bed",
  },
});
