# VideoOptimizer
For processing my university lecture videos to learn more efficiently. In C#.Net,  
1. Generate a transcript from audio with Microsoft Cognitive Services' Speech Recognition.  
2. Predict still frames from RGB statistics and determine when there is a new slide.  
3. The 'conversion' of a transcript from (1.) and images from (2.) to a printable (PDF/word) format.  
4. A video player with the ability to skip to the next significantly different slide, and skip sections of silence.  
(5. To do: audio analysis?)
### Compiling ###
Included are only the VS.NET solution classes I wrote. This provides the main functionality of the program and is enough to recreate it. It is not enough to compile anything useful on its own. (Missing: configurations, packages, designer code, other VS automatically-generated material.) Eventually I will configure NuGet Package Restore and upload the entire repository.
### Dependencies ###
Newtonsoft.Json, NReco.VideoConverter, NReco.VideoInfo, NAudio, Microsoft.CognitiveServices.Speech, Microsoft.Office.Interop.Word, .NET Framework...  
### Designer ###
Form1 : System.Windows.Forms.Form  
<img src="https://i.imgur.com/JX8BFVp.png" width="300"/>  
Form2 : System.Windows.Forms.Form  
<img src="https://i.imgur.com/cvlAFrI.png" width="200"/>  
voPlayer : System.Windows.Forms.Form  
<img src="https://i.imgur.com/RESaVd4.png" width="400"/>  
Example of a built application in execution  
<img src="https://i.imgur.com/8b4PxbS.png" width="550"/>
