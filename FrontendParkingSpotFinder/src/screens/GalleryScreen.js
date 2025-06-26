import React from "react";
import {
  View,
  Text,
  StyleSheet,
  FlatList,
  Image,
  TouchableOpacity,
} from "react-native";
import SpotCard from "../components/SpotCard";
import { LinearGradient } from "expo-linear-gradient";

const images = [
  {
    imageUrl: require("../../assets/images/imgpark.png"),
    timestamp: 1687804800000, // przykładowy timestamp w ms (np. 26 czerwca 2025)
  },
];

// const images = [
//   {
//     url: "https://example.com/photo1.jpg", // <--- tutaj link do obrazka z chmury
//     timestamp: 1687804800000, // np. UNIX timestamp w ms
//   },
//   {
//     url: "https://example.com/photo2.jpg",
//     timestamp: 1687891200000,
//   },
// ];

export default function GalleryScreen({ navigation }) {
  return (
    <LinearGradient
      colors={["#b993d6", "#8ca6db"]}
      start={{ x: 0, y: 0 }}
      end={{ x: 0, y: 1 }}
      style={styles.container}
    >
      <Text style={styles.header}>Gallery</Text>
      <Text style={styles.subHeader}>Free spots</Text>
      <FlatList
        data={images}
        renderItem={({ item }) => (
          <TouchableOpacity
            onPress={() =>
              navigation.navigate("PhotoScreen", {
                imageUrl: item.url,
                timestamp: item.timestamp,
              })
            }
            style={styles.imageWrapper}
          >
            <Image source={{ uri: item.url }} style={styles.image} />
          </TouchableOpacity>
        )}
        keyExtractor={(item, idx) => idx.toString()}
        contentContainerStyle={styles.list}
        showsVerticalScrollIndicator={false}
      />
    </LinearGradient>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    paddingTop: 48,
    paddingHorizontal: 35,
    backgroundColor: "#f7faff",
  },
  header: {
    fontFamily: "PressStart2P",
    fontSize: 20,
    color: "#3e1f8c",
    marginTop: 36,
    marginBottom: 15,
  },
  subHeader: {
    fontFamily: "PressStart2P",
    fontSize: 18,
    color: "#fff",
    marginBottom: 60,
  },
  row: {
    justifyContent: "space-between",
    marginBottom: 18,
  },
  list: {
    paddingBottom: 30,
  },
  imageWrapper: {
    flex: 1,
    margin: 8,
    borderRadius: 18,
    overflow: "hidden",
  },
  image: {
    width: 300,
    height: 300,
    borderRadius: 18,
  },
});
