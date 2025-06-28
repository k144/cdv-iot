import React from "react";
import { createStackNavigator } from "@react-navigation/stack";
import WelcomeScreen from "../screens/WelcomeScreen";
import GalleryScreen from "../screens/GalleryScreen";
import PhotoScreen from "../screens/PhotoScreen";

const Stack = createStackNavigator();

const AppNavigator = () => (
  <Stack.Navigator screenOptions={{ headerShown: false }}>
    <Stack.Screen name="Welcome" component={WelcomeScreen} />
    <Stack.Screen name="Gallery" component={GalleryScreen} />
    <Stack.Screen name="PhotoScreen" component={PhotoScreen} />
  </Stack.Navigator>
);

export default AppNavigator;
