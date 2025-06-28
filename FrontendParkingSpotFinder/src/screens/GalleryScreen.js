import { React, useEffect } from "react";

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

export default function GalleryScreen({ navigation }) {
  let images = [];
  useEffect(() => {
    for (let i = 1; i < 12; i++) {
      images.push({
        imageUrl: `https://www.popuw.com/images/maps/adomatic/level${i}.png`,
        timestamp: 1687804800000,
      });
    }
    console.log("dupa:");
    console.log(images);
  });
  // let images = [
  //   {
  //     imageUrl: "https://thispersondoesnotexist.com/",
  //     timestamp: 1687804800000, // przykładowy timestamp w ms (np. 26 czerwca 2025)
  //   },
  // ];

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
                imageUrl: item.imageUrl,
                timestamp: item.timestamp,
              })
            }
            style={styles.imageWrapper}
          >
            <Image source={{ uri: item.imageUrl }} style={styles.image} />
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
