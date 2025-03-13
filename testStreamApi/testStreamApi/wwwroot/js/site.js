//async function sendMessage() {
//    const response = await fetch("../Home/GetStream", {
//        method: "POST",
//        headers: { "Content-Type": "application/json" },
//        body: JSON.stringify({
//            message: "a123"
//        })
//    });

//    const reader = response.body.getReader();
//    const decoder = new TextDecoder();

//    while (true) {
//        const { value, done } = await reader.read();
//        if (done) break;
//        const text = decoder.decode(value, { stream: true }); // Giữ lại nội dung trước đó
//        console.log("Received:", text);
//        document.body.innerHTML += `<p>${text}</p>`;
//    }
//}

async function sendMessage() {
    try {
        const response = await fetch("../Home/GetStream", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ message: "a123" })
        });

        if (!response.ok) {
            throw new Error(`HTTP error! Status: ${response.status}`);
        }

        const reader = response.body.getReader();
        const decoder = new TextDecoder();
        const outputDiv = document.getElementById("p-t");

        outputDiv.innerHTML += `<p><strong>Đang nhận dữ liệu...</strong></p>`;

        let buffer = "";  // Bộ đệm dữ liệu để ghép JSON bị cắt
        while (true) {
            const { value, done } = await reader.read();
            if (done) break;

            buffer += decoder.decode(value, { stream: true }); // Ghép phần còn lại từ lần đọc trước

            console.log("Received:", text);

            // Cập nhật nội dung từng phần mà không chặn UI
            outputDiv.innerHTML += `<span>${text}</span>`;


            let lines = buffer.split("\n\n"); // Tách dữ liệu theo dòng SSE
            buffer = lines.pop(); // Giữ lại phần cuối nếu chưa đủ dữ liệu

            for (const line of lines) {
                if (line.startsWith("data: ")) {
                    const jsonString = line.substring(6); // Bỏ "data: "
                    try {
                        const eventData = JSON.parse(jsonString);

                        // 🔹 Giải mã Base64 để lấy dữ liệu gốc
                        const decodedData = atob(eventData.Data);

                        if (eventData.Type === "MetaData") {
                            document.getElementById("metadata").innerText = "MetaData: " + decodedData;
                        } else if (eventData.Type === "Tokens") {
                            conversationDiv.innerText += decodedData + " ";
                        }
                    } catch (error) {
                        console.error("JSON Parse Error:", error, "Raw Data:", jsonString);
                    }
                }
            }



        }

    } catch (error) {
        console.error("Fetch error:", error);
        document.getElementById("output").innerHTML += `<p style="color: red;">Lỗi: ${error.message}</p>`;
    }
}


sendMessage();
