![Logo](https://github.com/MATHEMA-GmbH/Owl-Racer-AI/blob/main/doc/owlracer-logo.png?raw=true)

# Owl Racer GUI


<p align="center">

  ## Get Conected

  <a href="https://de.linkedin.com/company/mathema-gmbh" align="center" >
          <img src="https://img.shields.io/badge/LinkedIn-0077B5?style=for-the-badge&logo=linkedin&logoColor=white" /></a>

  <a href="https://www.youtube.com/channel/UC0vntD32UJckGUXcVvlrIiA">
          <img src="https://img.shields.io/badge/YouTube-FF0000?style=for-the-badge&logo=youtube&logoColor=white" /></a>

  <a href="https://twitter.com/MATHEMA_GmbH">
          <img src="https://img.shields.io/badge/Twitter-1DA1F2?style=for-the-badge&logo=twitter&logoColor=white" /></a>

  <a href="https://www.facebook.com/mathema.software.gmbh/">
            <img src="https://img.shields.io/badge/Facebook-1877F2?style=for-the-badge&logo=facebook&logoColor=white" /></a>

</p></center>

____

<a href="https://www.mathema.de/blog">
        <img src="https://img.shields.io/badge/Blog%20Article-1-green?style=social" /></a>

____

### General information

The UI client is a visual test and sandbox client implemented with the MonoGame framework. MonoGame is based on Microsofts XNA framework and enables fast and lightweight 2D rendering perfectly suited for the purpose of showing the user what is going on during machine learning. It also enables the users to drive a car themselves and even compete against AI.

Note however, that the UI client is merely a tool to give developers a visual representation to make analysis easier (ok, and to incorporate some fun into the project ^^). The focus still relies on the machine learning clients doing the actually interesting stuff.

The communication between the UI client and the server is realized using a (Web-)gRPC channel.

The UI currently supports Resolutions above 1920 x 1200.

____

### Key bindings

#### General key bindings:

Upward Key: 		Accelerate car <br>
Downward Key: 		Decelerate car <br>
Left Key: 			Turn left <br>
Right Key: 			Turn right <br>
Space: 			Reset position of car <br>

Esc in gamestate: 	Quit session <br>
Esc in rankingstate:	Quit ranking state <br>
D: 				Shows car statistics <br>
K: 				Toggles darkmode on/off <br>
L: 				Logs the games data <br>

(Logged data can be found in "./Matlabs.OwlRacer.GameClient/bin/Debug/net6.0/capture/")

#### Model keybindings & appsettings.json:

In order for these key bindings to work you have to adjust the appsettings.json file. <br>
You find examples of correct relative filepaths in the apssettings_example.json file. <br>
Make sure to replace "<your_path_to_python.exe>" with the correct file path to your <br>
installed python.exe <br>

Python Models:    
F1:				spawn simpleML_DT_deprecated model <br>
F2:				spawn simpleML_RF_deprecated model <br>	
F3:				Spawn DecisionTree_(Py) model <br>

ML.Net Models:
F4: 				spawn DNN_(ML.Net) mode <br>		
F5: 				spawn DecisionTreeClassifier_(ML.NET) model <br>
F6: 				spawn RandomForest_deprecated(ML.NET) model <br>

____

For more details on the total project read [here](https://github.com/MATHEMA-GmbH/Owl-Racer-AI).
