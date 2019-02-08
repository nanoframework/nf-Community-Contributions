
# nanoFramework Driver for the 74HC595 Shift Register

[Source Code](https://github.com/Dweaver309/Shift-Register-74HC595/tree/Source_Code)


![ScreenShot](https://github.com/Dweaver309/Shift-Register-74HC595/blob/Images/ShiftRegisterFritzing.png)

# Shift Register Tutorial


**74HC595** shift register enables up to 8 additional output ports using only three ports from the device. More ports can be added by "daisy chaining" more chips. 



## The hardware

![ScreenShot](https://github.com/Dweaver309/Shift-Register-74HC595/blob/Images/74HC595Pins.png
)

- The Q0 to Q7 pins are the new output pins they are in reverse order Q7 is Pin 0 and Q0 is Pin 7

- VCC is connected to 3.3 volts or 5 volts

- Data, Latch, Clock are connected to any three digital pins 

- Output Enable is connected to ground

- Master Reset is connected to VCC

- GND is connected to ground

## Understanding the software

- The driver is first initialized by calling the constructor like this: `HC595 ShiftRegister = new HC595(Clock, Data, Latch)`

- The pin state is changed by the method SetPin(Pin,State) Example: `ShiftRegister.SetPin(7, false)`

- The SetPin method changes the pin state doing the following
