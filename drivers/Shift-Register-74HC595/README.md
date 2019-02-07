
# nanoFramework Driver for the 74HC595 Shift Register

[Source Code](https://github.com/Dweaver309/nf-Community-Contributions/tree/master/drivers/Shift-Register-74HC595/Source_Code)


![ScreenShot](https://github.com/Dweaver309/nf-Community-Contributions/blob/master/drivers/Shift-Register-74HC595/Images/ShiftRegisterFritzing.png)

# Shift Register Tutoral


**74HC595** shift register enables put to 8 additional output ports using only three ports from the device. More ports can be added by "daisy chaining" more chips. 




You can find the video for this guide  [here](https://github.com/Dweaver309/nf-Community-Contributions/blob/master/drivers/Shift-Register-74HC595/Images/ShiftRegister.MOV).


## The hardware

![ScreenShot](https://github.com/Dweaver309/nf-Community-Contributions/blob/master/drivers/Shift-Register-74HC595/Images/74HC595Pins.png)

- The Q1 to Q7 pins are the new output pins they are in reverse order Q7 is Pin 0 and Q1 is Pin 7

- VCC is connected to 3.3 volts or 5 volts

- Data, Latch, Clock are connected to any three digital pins 

- Output Enable is connected to ground

- Master Reset is connected to VCC

- GND is connected to the devices ground


Contributor: @Dweaver309
