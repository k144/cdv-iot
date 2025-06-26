import React from "react";
import { View, StyleSheet } from "react-native";

export default function SpotCard() {
  return <View style={styles.card} />;
}

const styles = StyleSheet.create({
  card: {
    width: 140,
    height: 160,
    backgroundColor: "#fff",
    borderRadius: 20,
    marginBottom: 10,
    shadowColor: "#000",
    shadowOpacity: 0.08,
    shadowRadius: 8,
    elevation: 2,
  },
});
