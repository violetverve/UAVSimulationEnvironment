import socket
import cv2
import numpy as np

HOST = '0.0.0.0'
PORT = 5000

# GStreamer pipeline for streaming
gst_pipeline = 'appsrc ! videoconvert ! x264enc tune=zerolatency bitrate=500 speed-preset=ultrafast ! rtph264pay ! udpsink host=192.168.1.103 port=5001'


# Initialize the GStreamer pipeline with VideoWriter
fourcc = cv2.VideoWriter_fourcc(*'X264')
fps = 15
width, height = 256, 256
out = cv2.VideoWriter(gst_pipeline, fourcc, fps, (width, height), isColor=True)

server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
server_socket.bind((HOST, PORT))
server_socket.listen(1)
print('Server listening on port', PORT)

conn, addr = server_socket.accept()
print('Connected by', addr)

dropped = False

while True:
    # Receive the length of the image data
    length_prefix = conn.recv(4)
    if not length_prefix:
        print("Connection closed or no data received.")
        break
    data_length = int.from_bytes(length_prefix, byteorder='little')
    print(f"Expected image data length: {data_length} bytes")

    # Receive the image data
    image_data = b''
    while len(image_data) < data_length:
        packet = conn.recv(data_length - len(image_data))
        if not packet:
            print("Connection closed or incomplete data received.")
            break
        image_data += packet
    print(f"Received image data of length: {len(image_data)} bytes")

    # Decode the image data to a numpy array
    img_array = np.frombuffer(image_data, dtype=np.uint8)
    frame = cv2.imdecode(img_array, cv2.IMREAD_COLOR)

    if frame is None:
        print("Failed to decode the image data.")
        continue

    # Convert the frame to HSV color space
    hsv_frame = cv2.cvtColor(frame, cv2.COLOR_BGR2HSV)
    
    # Stream the frame using VideoWriter
    out.write(frame)

    # Define the range for red color in HSV
    lower_red1 = np.array([0, 120, 70])
    upper_red1 = np.array([10, 255, 255])
    mask1 = cv2.inRange(hsv_frame, lower_red1, upper_red1)

    lower_red2 = np.array([170, 120, 70])
    upper_red2 = np.array([180, 255, 255])
    mask2 = cv2.inRange(hsv_frame, lower_red2, upper_red2)

    # Combine the masks
    red_mask = mask1 | mask2

    # Check if there is red color in the center of the frame
    height, width = red_mask.shape
    center_x, center_y = width // 2, height // 2
    red_center = red_mask[center_y-10:center_y+10, center_x-10:center_x+10]
    
    if (not dropped):
        if np.any(red_center):
            command = "DROP"
            dropped = True
        else:
            command = "WANDER"

    # Send control command back to the client
    command_bytes = command.encode('utf-8')
    conn.sendall(command_bytes)

conn.close()
server_socket.close()
out.release()
print("Server closed.")
