PADI project: PADI-DTSM
=========
To test the project, follow the next steps:

1. Open the solution (PADI-DSTM.sln file) in Visual Studio;
2. Then, in the Solution Explorer of Visual Studio, rigtht click on the solution and go to Properties;
3. In the menu that opened, click on the radio button "Multiple startup projects" and make the projects PADI-DSTM_Client, PADI-DSTM_DataServer and PADI-DSTM_Master start;
4. Then, finally, press F5 to run.

In the DataServer and the Client, you need to choose the port on which you want to run them. PLEASE don't choose the port 9999, because that's the port of the Master server.
If you want to start another DataServer or Client, go to bin folder of the project and double click on the .exe file.