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

        while (true) {
            const { value, done } = await reader.read();
            if (done) break;

            const text = decoder.decode(value, { stream: true });
            console.log("Received:", text);

            // Cập nhật nội dung từng phần mà không chặn UI
            outputDiv.innerHTML += `<span>${text}</span>`;
        }

    } catch (error) {
        console.error("Fetch error:", error);
        document.getElementById("output").innerHTML += `<p style="color: red;">Lỗi: ${error.message}</p>`;
    }
}


sendMessage();
