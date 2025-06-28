import React from "react";
import { View, Text, Image, StyleSheet } from "react-native";
import PixelButton from "../components/PixelButton";
import { LinearGradient } from "expo-linear-gradient";

export default function WelcomeScreen({ navigation }) {
  return (
    <LinearGradient
      colors={["#b993d6", "#8ca6db"]}
      start={{ x: 0, y: 0 }}
      end={{ x: 0, y: 1 }}
      style={styles.container}
    >
      <Text style={styles.title}>Find your free spot</Text>
      <PixelButton
        label="Check free spot"
        onPress={() => navigation.navigate("Gallery")}
      />
      <Image
        source={require("../../assets/images/pixelcar.png")}
        style={styles.pixelcar}
      />
    </LinearGradient>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    alignItems: "center",
    justifyContent: "center",
    padding: 24,
  },
  title: {
    fontFamily: "PressStart2P",
    fontSize: 20,
    color: "#fff",
    textAlign: "center",
    marginBottom: 32,
    lineHeight: 36,
  },
  pixelcar: {
    width: 360,
    height: 320,
    marginTop: 5,
    resizeMode: "contain",
  },
});
