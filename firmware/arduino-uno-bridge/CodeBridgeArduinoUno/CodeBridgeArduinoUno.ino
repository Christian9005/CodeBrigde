#include <Arduino.h>

#define CODEBRIDGE_FIRMWARE_VERSION "0.8.0-uno"
#define LINE_BUFFER_SIZE 96
#define MAX_SERVOS 4

struct ServoSlot {
  int pin;
  int angle;
  int pulseUs;
  unsigned long lastRefreshUs;
  bool attached;
};

static char lineBuffer[LINE_BUFFER_SIZE];
static size_t lineLength = 0;
static ServoSlot servos[MAX_SERVOS];

static void sendOK() {
  Serial.println("OK");
}

static void sendOK(const char* value) {
  Serial.print("OK:");
  Serial.println(value);
}

static void sendOK(int value) {
  Serial.print("OK:");
  Serial.println(value);
}

static void sendError(const char* message) {
  Serial.print("ERR:");
  Serial.println(message);
}

static int nextParam(char*& ptr) {
  if (ptr == nullptr || *ptr == '\0') {
    return 0;
  }

  char* end = ptr;
  long value = strtol(ptr, &end, 10);
  ptr = (*end == ':') ? end + 1 : end;
  return (int)value;
}

static bool validDigitalPin(int pin) {
  return pin >= 0 && pin <= 19;
}

static bool validAnalogPin(int pin) {
  return (pin >= 14 && pin <= 19) || (pin >= A0 && pin <= A5);
}

static int findServo(int pin) {
  for (int i = 0; i < MAX_SERVOS; i++) {
    if (servos[i].attached && servos[i].pin == pin) {
      return i;
    }
  }

  return -1;
}

static int findFreeServoSlot() {
  for (int i = 0; i < MAX_SERVOS; i++) {
    if (!servos[i].attached) {
      return i;
    }
  }

  return -1;
}

static int angleToPulseUs(int angle) {
  return map(constrain(angle, 0, 180), 0, 180, 544, 2400);
}

static void updateServos() {
  unsigned long now = micros();
  for (int i = 0; i < MAX_SERVOS; i++) {
    if (!servos[i].attached) {
      continue;
    }

    if ((unsigned long)(now - servos[i].lastRefreshUs) < 20000UL) {
      continue;
    }

    digitalWrite(servos[i].pin, HIGH);
    delayMicroseconds(servos[i].pulseUs);
    digitalWrite(servos[i].pin, LOW);
    servos[i].lastRefreshUs = micros();
  }
}

static void handlePinMode(char* params) {
  int pin = nextParam(params);
  int mode = nextParam(params);

  if (!validDigitalPin(pin)) {
    sendError("Invalid pin");
    return;
  }

  switch (mode) {
    case 0: pinMode(pin, INPUT); break;
    case 1: pinMode(pin, OUTPUT); break;
    case 2: pinMode(pin, INPUT_PULLUP); break;
    case 3: pinMode(pin, INPUT_PULLUP); break;
    case 4: break;
    default:
      sendError("Invalid mode");
      return;
  }

  sendOK();
}

static void handleDigitalWrite(char* params) {
  int pin = nextParam(params);
  int value = nextParam(params);

  if (!validDigitalPin(pin)) {
    sendError("Invalid pin");
    return;
  }

  digitalWrite(pin, value ? HIGH : LOW);
  sendOK();
}

static void handleDigitalRead(char* params) {
  int pin = nextParam(params);

  if (!validDigitalPin(pin)) {
    sendError("Invalid pin");
    return;
  }

  sendOK(digitalRead(pin) == HIGH ? 1 : 0);
}

static void handleAnalogRead(char* params) {
  int pin = nextParam(params);

  if (!validAnalogPin(pin)) {
    sendError("Invalid analog pin");
    return;
  }

  sendOK(analogRead(pin));
}

static void handlePwmWrite(char* params) {
  int pin = nextParam(params);
  int duty = constrain(nextParam(params), 0, 255);
  (void)nextParam(params);

  if (!validDigitalPin(pin)) {
    sendError("Invalid pin");
    return;
  }

  analogWrite(pin, duty);
  sendOK();
}

static void handleServoAttach(char* params) {
  int pin = nextParam(params);
  int minPulse = nextParam(params);
  int maxPulse = nextParam(params);

  if (!validDigitalPin(pin)) {
    sendError("Invalid pin");
    return;
  }

  if (minPulse <= 0) {
    minPulse = 544;
  }

  if (maxPulse <= 0) {
    maxPulse = 2400;
  }

  int slot = findServo(pin);
  if (slot < 0) {
    slot = findFreeServoSlot();
  }

  if (slot < 0) {
    sendError("No servo slots");
    return;
  }

  servos[slot].pin = pin;
  servos[slot].angle = 90;
  servos[slot].pulseUs = constrain(angleToPulseUs(90), minPulse, maxPulse);
  servos[slot].lastRefreshUs = 0;
  servos[slot].attached = true;
  pinMode(pin, OUTPUT);
  sendOK();
}

static void handleServoWrite(char* params) {
  int pin = nextParam(params);
  int angle = constrain(nextParam(params), 0, 180);
  int slot = findServo(pin);

  if (slot < 0) {
    sendError("Servo not attached");
    return;
  }

  servos[slot].angle = angle;
  servos[slot].pulseUs = angleToPulseUs(angle);
  sendOK();
}

static void handleServoRead(char* params) {
  int pin = nextParam(params);
  int slot = findServo(pin);

  if (slot < 0) {
    sendError("Servo not attached");
    return;
  }

  sendOK(servos[slot].angle);
}

static void handleServoMicroseconds(char* params) {
  int pin = nextParam(params);
  int microseconds = nextParam(params);
  int slot = findServo(pin);

  if (slot < 0) {
    sendError("Servo not attached");
    return;
  }

  servos[slot].pulseUs = constrain(microseconds, 500, 2500);
  sendOK();
}

static void handleServoDetach(char* params) {
  int pin = nextParam(params);
  int slot = findServo(pin);

  if (slot < 0) {
    sendError("Servo not attached");
    return;
  }

  digitalWrite(servos[slot].pin, LOW);
  servos[slot].attached = false;
  sendOK();
}

static void processCommand(char* line) {
  char* params = strchr(line, ':');
  if (params != nullptr) {
    *params = '\0';
    params++;
  } else {
    params = line + strlen(line);
  }

  if (strcmp(line, "PING") == 0) {
    sendOK("PONG");
  } else if (strcmp(line, "VER") == 0) {
    sendOK(CODEBRIDGE_FIRMWARE_VERSION);
  } else if (strcmp(line, "INFO") == 0) {
    sendOK("{\"chip\":\"ATmega328P\",\"freq\":16,\"heap\":0,\"flash\":32768,\"sdk\":\"arduino-avr\"}");
  } else if (strcmp(line, "RST") == 0) {
    sendOK("RESET");
    delay(50);
    asm volatile ("jmp 0");
  } else if (strcmp(line, "PM") == 0) {
    handlePinMode(params);
  } else if (strcmp(line, "DW") == 0) {
    handleDigitalWrite(params);
  } else if (strcmp(line, "DR") == 0) {
    handleDigitalRead(params);
  } else if (strcmp(line, "AR") == 0) {
    handleAnalogRead(params);
  } else if (strcmp(line, "PW") == 0) {
    handlePwmWrite(params);
  } else if (strcmp(line, "SA") == 0) {
    handleServoAttach(params);
  } else if (strcmp(line, "SV") == 0) {
    handleServoWrite(params);
  } else if (strcmp(line, "SVR") == 0) {
    handleServoRead(params);
  } else if (strcmp(line, "SU") == 0) {
    handleServoMicroseconds(params);
  } else if (strcmp(line, "SD") == 0) {
    handleServoDetach(params);
  } else {
    sendError("Unsupported command");
  }
}

void setup() {
  Serial.begin(115200);
  delay(300);
  Serial.println("CODEBRIDGE_READY");
}

void loop() {
  while (Serial.available() > 0) {
    char ch = (char)Serial.read();
    if (ch == '\r') {
      continue;
    }

    if (ch == '\n') {
      lineBuffer[lineLength] = '\0';
      if (lineLength > 0) {
        processCommand(lineBuffer);
      }
      lineLength = 0;
      continue;
    }

    if (lineLength < LINE_BUFFER_SIZE - 1) {
      lineBuffer[lineLength++] = ch;
    } else {
      lineLength = 0;
      sendError("Line too long");
    }
  }

  updateServos();
}
