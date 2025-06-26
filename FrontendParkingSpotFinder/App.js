import React from "react";
import { NavigationContainer } from "@react-navigation/native";
import AppNavigator from "./src/navigation/AppNavigator";
import { View, ActivityIndicator, StyleSheet } from "react-native";
import * as Font from "expo-font"; // Importuj Font z expo-font
import { useEffect, useState } from "react"; // Dodaj useEffect i useState

export default function App() {
  const [fontsLoaded, setFontsLoaded] = useState(false); // Zmień na useState

  useEffect(() => {
    async function loadResourcesAndDataAsync() {
      try {
        await Font.loadAsync({
          PressStart2P: require("./assets/fonts/PressStart2P_Regular.ttf"), // Ważne: nazwa i ścieżka!
          // Dodaj inne fonty, jeśli masz
        });
      } catch (e) {
        // Możesz tutaj obsłużyć błędy ładowania fontów
        console.warn(e);
      } finally {
        setFontsLoaded(true); // Ustaw flagę na true po załadowaniu
      }
    }

    loadResourcesAndDataAsync();
  }, []); // Pusta tablica zależności oznacza, że uruchomi się tylko raz po zamontowaniu

  if (!fontsLoaded) {
    // Pokaż ekran ładowania, dopóki fonty się nie załadują
    return (
      <View style={styles.loadingContainer}>
        <ActivityIndicator size="large" color="#0000ff" />
      </View>
    );
  }

  // Po załadowaniu fontów, renderuj aplikację
  return (
    <NavigationContainer>
      <AppNavigator />
    </NavigationContainer>
  );
}

const styles = StyleSheet.create({
  loadingContainer: {
    flex: 1,
    justifyContent: "center",
    alignItems: "center",
    backgroundColor: "#fff",
  },
});
