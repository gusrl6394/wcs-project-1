import os
import time
import random
import json
from flask import Flask

app = Flask(__name__)

# Configuration for the temperature sensors
NUM_SENSORS = 10
MIN_TEMP = 1.9
MAX_TEMP = 14.0
SEND_INTERVAL_SECONDS = 5  # Send data every 5 seconds


# Initialize current temperatures for each sensor randomly within the range
# Using a list to store temperatures for each sensor
current_temperatures = [round(random.uniform(MIN_TEMP, MAX_TEMP), 1) for _ in range(NUM_SENSORS)]

def generate_temperature_for_sensor(sensor_id):
    """
    Generates a new temperature for a given sensor, with slight variations
    and ensuring it stays within the defined min/max range.
    """
    global current_temperatures
    
    # Introduce a small random change
    change = random.uniform(-0.2, 0.2)
    new_temp = current_temperatures[sensor_id] + change

    # Ensure temperature stays within bounds
    if new_temp < MIN_TEMP:
        new_temp = MIN_TEMP + random.uniform(0, 0.1) # Slightly bounce back from min
    elif new_temp > MAX_TEMP:
        new_temp = MAX_TEMP - random.uniform(0, 0.1) # Slightly bounce back from max
    
    # 저장할 때 소수점 2자리로 반올림
    current_temperatures[sensor_id] = round(new_temp, 2)
    return current_temperatures[sensor_id]

# Flask application routes
@app.route('/temperatures')
def temperatures():
    """Returns the current temperatures of all sensors."""
    temperatures_data = []
    # 각 요청 시 센서값을 갱신하고(변동 시뮬레이션), 반올림된 값을 응답에 포함
    for i in range(NUM_SENSORS):
        temp = generate_temperature_for_sensor(i)
        temperatures_data.append({
            "sensorId": f"Sensor-{i+1}",
            "temperature": temp
        })
    return json.dumps(temperatures_data), 200, {'Content-Type': 'application/json'}


if __name__ == '__main__':
    HOST = os.environ.get('SERVER_HOST', 'localhost')
    app.run(HOST, 5101)